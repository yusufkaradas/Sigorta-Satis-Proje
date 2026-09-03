import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PolicyEdit } from './policy-edit';

describe('PolicyEdit', () => {
  let component: PolicyEdit;
  let fixture: ComponentFixture<PolicyEdit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyEdit],
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyEdit);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
