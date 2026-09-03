import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PolicyDetail } from './policy-detail';

describe('PolicyDetail', () => {
  let component: PolicyDetail;
  let fixture: ComponentFixture<PolicyDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyDetail],
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
